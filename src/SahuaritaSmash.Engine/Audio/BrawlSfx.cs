// Classic Games - Sahuarita Smash!, part 6: the sounds, all made in code: cartoon bonks and whooshes, never anything that
// sounds like someone getting hurt. Hits are a soft thump with a little crack on top (bigger hits are deeper and
// longer), flying off the stage is a boom and a rising whistle, off the top a twinkle. Each stage surprise has its own
// warning sound (a bell, a beep, a hum, a whistle, the wind, the javelinas snorting).
using System;

namespace ClassicGames
{
    public static class BrawlSfx
    {
        const int Rate = Audio.Rate;
        public static readonly string[] Names = {
            "brawl-swing", "brawl-swish", "brawl-strong", "brawl-star", "brawl-dash", "brawl-copter", "brawl-bubble", "brawl-lash", "brawl-burst",
            "brawl-blaster", "brawl-throw", "brawl-jet", "brawl-drill", "brawl-whoosh", "brawl-flap", "brawl-needles", "brawl-bloom", "brawl-moose",
            "brawl-counter", "brawl-zap", "brawl-packet", "brawl-fire", "brawl-uplink", "brawl-spin", "brawl-special", "brawl-jump", "brawl-djump",
            "brawl-dodge", "brawl-break", "brawl-thud", "brawl-grab", "brawl-pummel", "brawl-ledge", "brawl-shieldhit", "brawl-counter-hit",
            "brawl-smash", "brawl-hit1", "brawl-hit2", "brawl-reflect", "brawl-fizzle", "brawl-boom", "brawl-bell", "brawl-beep", "brawl-hum",
            "brawl-whistle", "brawl-wind", "brawl-twinkle", "brawl-ko", "brawl-sudden", "brawl-game", "brawl-go",
            "brawl-snort", "brawl-squeal", "brawl-hooves" };

        static float[] Buf(double s) { return new float[(int)(s * Rate)]; }

        /// <summary>A tone sweeping from f0 to f1: wave 0 square, 1 thin pulse, 2 triangle, 3 saw, 4 sine.</summary>
        static void Tone(float[] b, double start, double len, double f0, double f1, int wave, double vol, double decay)
        {
            int s0 = (int)(start * Rate), n = (int)(len * Rate);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                if (s0 + i < 0) continue;
                double t = (double)i / Rate, k = (double)i / n;
                double f = f0 * Math.Pow(f1 / f0, k);
                ph += f / Rate; ph -= Math.Floor(ph);
                double w;
                switch (wave)
                {
                    case 0: w = ph < 0.5 ? 1 : -1; break;
                    case 1: w = ph < 0.2 ? 1 : -1; break;
                    case 2: w = ph < 0.5 ? 4 * ph - 1 : 3 - 4 * ph; break;
                    case 3: w = 2 * ph - 1; break;
                    default: w = Math.Sin(ph * 2 * Math.PI); break;
                }
                double env = Math.Exp(-t * decay) * Math.Min(1, i / 60.0) * Math.Min(1, (n - i) / 260.0);
                b[s0 + i] += (float)(w * env * vol);
            }
        }

        static void Noise(float[] b, double start, double len, double vol, int hold, double decay, int seed)
        {
            uint r = (uint)(seed * 2654435761u + 13);
            int s0 = (int)(start * Rate), n = (int)(len * Rate);
            double v = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                if (i % Math.Max(1, hold) == 0) { r ^= r << 13; r ^= r >> 17; r ^= r << 5; v = (r % 20000) / 10000.0 - 1; }
                double t = (double)i / Rate;
                double env = Math.Exp(-t * decay) * Math.Min(1, i / 40.0) * Math.Min(1, (n - i) / 260.0);
                b[s0 + i] += (float)(v * env * vol);
            }
        }

        static void Lowpass(float[] b, double k)
        {
            double a = 0, c = 0;
            for (int i = 0; i < b.Length; i++) { a += (b[i] - a) * k; c += (a - c) * k; b[i] = (float)c; }
        }

        static void Notes(float[] b, double start, double each, int wave, double vol, params int[] midi)
        {
            for (int i = 0; i < midi.Length; i++) { double f = Song.NoteFreq(midi[i]); Tone(b, start + i * each, each * 1.6, f, f, wave, vol, 6); }
        }

        static float[] Peak(float[] b, double peak)
        {
            float m = 0; foreach (float v in b) m = Math.Max(m, Math.Abs(v));
            if (m > peak && m > 0) { float k = (float)(peak / m); for (int i = 0; i < b.Length; i++) b[i] *= k; }
            int fo = Math.Min(b.Length, (int)(0.006 * Rate));
            for (int i = 0; i < fo; i++) b[b.Length - 1 - i] *= (float)i / fo;
            return b;
        }

        static float[] Whoosh(double len, double f0, double f1, double vol, int seed)
        {
            float[] b = Buf(len);
            Noise(b, 0, len, vol, 1, 3, seed);
            Lowpass(b, 0.25);
            Tone(b, 0, len, f0, f1, 4, vol * 0.3, 4);
            return b;
        }

        public static float[] Make(string name)
        {
            float[] b;
            switch (name)
            {
                case "brawl-swish": b = Whoosh(0.12, 900, 500, 0.35, 1); break;
                case "brawl-swing": b = Whoosh(0.18, 700, 300, 0.42, 2); break;
                case "brawl-strong": b = Whoosh(0.3, 500, 160, 0.5, 3); Tone(b, 0, 0.2, 140, 80, 4, 0.2, 8); break;
                case "brawl-whoosh": b = Whoosh(0.35, 300, 900, 0.4, 4); break;
                case "brawl-dash": b = Whoosh(0.3, 400, 1200, 0.42, 5); Tone(b, 0, 0.25, 600, 1400, 2, 0.08, 4); break;
                case "brawl-star": b = Buf(0.3); Notes(b, 0, 0.05, 2, 0.16, 84, 88, 91, 96); break;
                case "brawl-copter": b = Buf(0.6); for (int i = 0; i < 10; i++) Noise(b, i * 0.055, 0.035, 0.22, 2, 30, 10 + i); Lowpass(b, 0.4); break;
                case "brawl-bubble": b = Buf(0.35); Tone(b, 0, 0.3, 300, 900, 4, 0.3, 5); Tone(b, 0.05, 0.2, 600, 1500, 4, 0.12, 8); break;
                case "brawl-lash": b = Whoosh(0.22, 1200, 400, 0.4, 6); Noise(b, 0.12, 0.06, 0.3, 1, 30, 7); break;
                case "brawl-burst": b = Buf(0.45); Noise(b, 0, 0.4, 0.45, 3, 7, 8); Lowpass(b, 0.3); Tone(b, 0, 0.3, 200, 70, 4, 0.35, 6); break;
                case "brawl-blaster": b = Buf(0.16); Tone(b, 0, 0.14, 1800, 500, 1, 0.14, 12); Noise(b, 0, 0.04, 0.15, 1, 40, 9); break;
                case "brawl-throw": b = Whoosh(0.22, 500, 900, 0.38, 11); break;
                case "brawl-jet": b = Buf(0.7); Noise(b, 0, 0.65, 0.35, 1, 1.5, 12); Lowpass(b, 0.18); Tone(b, 0, 0.6, 90, 140, 3, 0.08, 1); break;
                case "brawl-drill": b = Buf(0.6); for (int i = 0; i < 14; i++) Tone(b, i * 0.04, 0.04, 260 + (i % 2) * 40, 240, 3, 0.12, 10); Noise(b, 0, 0.55, 0.08, 4, 2, 13); break;
                case "brawl-flap": b = Buf(0.45); for (int i = 0; i < 5; i++) Noise(b, i * 0.08, 0.06, 0.3, 2, 25, 14 + i); Lowpass(b, 0.3); break;
                case "brawl-needles": b = Buf(0.25); for (int i = 0; i < 3; i++) Tone(b, i * 0.05, 0.08, 2400, 1600, 1, 0.08, 20); break;
                case "brawl-bloom": b = Buf(0.5); Notes(b, 0, 0.07, 4, 0.2, 72, 76, 79, 84); Tone(b, 0, 0.45, 400, 1200, 4, 0.08, 3); break;
                case "brawl-moose": b = Buf(0.7); Tone(b, 0, 0.6, 170, 120, 3, 0.2, 2); Tone(b, 0, 0.6, 172, 118, 2, 0.15, 2); Lowpass(b, 0.25); Noise(b, 0, 0.6, 0.06, 30, 1, 15); break;
                case "brawl-counter": b = Buf(0.3); Tone(b, 0, 0.25, 700, 700, 1, 0.1, 6); Tone(b, 0.02, 0.2, 1050, 1050, 1, 0.06, 8); break;
                case "brawl-counter-hit": b = Buf(0.5); Notes(b, 0, 0.06, 0, 0.12, 76, 83, 88); Noise(b, 0, 0.12, 0.3, 1, 20, 16); Tone(b, 0, 0.3, 160, 60, 4, 0.4, 7); break;
                case "brawl-zap": b = Buf(0.35); for (int i = 0; i < 8; i++) Noise(b, i * 0.035, 0.03, 0.25, 1, 20, 17 + i); Tone(b, 0, 0.3, 2000, 800, 1, 0.05, 6); break;
                case "brawl-packet": b = Buf(0.2); Notes(b, 0, 0.04, 1, 0.1, 91, 86, 93); break;
                case "brawl-fire": b = Buf(0.6); Noise(b, 0, 0.55, 0.4, 2, 3, 26); Lowpass(b, 0.2); Tone(b, 0, 0.3, 120, 200, 3, 0.1, 4); break;
                case "brawl-uplink": b = Buf(0.5); Tone(b, 0, 0.45, 300, 1400, 1, 0.1, 2); Tone(b, 0, 0.45, 310, 1420, 4, 0.12, 2); break;
                case "brawl-spin": b = Buf(0.5); for (int i = 0; i < 6; i++) { double s = i * 0.07; float[] w = Whoosh(0.07, 800, 400, 0.3, 27 + i); for (int j = 0; j < w.Length && (int)(s * Rate) + j < b.Length; j++) b[(int)(s * Rate) + j] += w[j]; } break;
                case "brawl-special": b = Whoosh(0.2, 600, 1000, 0.3, 33); break;
                case "brawl-jump": b = Buf(0.14); Tone(b, 0, 0.12, 300, 700, 2, 0.16, 10); break;
                case "brawl-djump": b = Buf(0.18); Tone(b, 0, 0.16, 400, 1000, 2, 0.14, 8); Noise(b, 0, 0.1, 0.08, 1, 20, 34); Lowpass(b, 0.5); break;
                case "brawl-dodge": b = Whoosh(0.16, 1200, 700, 0.3, 35); break;
                case "brawl-break": b = Buf(0.8); for (int i = 0; i < 6; i++) Tone(b, i * 0.06, 0.15, 2400 - i * 250, 1600 - i * 150, 4, 0.12, 8); Noise(b, 0, 0.3, 0.3, 1, 10, 36); break;
                case "brawl-thud": b = Buf(0.25); Tone(b, 0, 0.22, 110, 50, 4, 0.55, 14); Noise(b, 0, 0.08, 0.2, 3, 30, 37); Lowpass(b, 0.3); break;
                case "brawl-grab": b = Buf(0.15); Noise(b, 0, 0.1, 0.3, 2, 30, 38); Lowpass(b, 0.3); Tone(b, 0, 0.1, 250, 180, 4, 0.2, 20); break;
                case "brawl-pummel": b = Buf(0.12); Tone(b, 0, 0.1, 220, 120, 4, 0.4, 25); Noise(b, 0, 0.04, 0.2, 1, 40, 39); break;
                case "brawl-ledge": b = Buf(0.12); Tone(b, 0, 0.1, 500, 380, 2, 0.15, 20); break;
                case "brawl-shieldhit": b = Buf(0.25); Tone(b, 0, 0.22, 900, 700, 4, 0.2, 10); Tone(b, 0, 0.22, 1350, 1100, 4, 0.08, 12); break;
                // hits: a soft thump with a little crack on top; the bigger the hit, the deeper and longer
                case "brawl-hit1": b = Buf(0.18); Tone(b, 0, 0.16, 260, 110, 4, 0.5, 18); Noise(b, 0, 0.05, 0.28, 1, 50, 40); Lowpass(b, 0.5); break;
                case "brawl-hit2": b = Buf(0.28); Tone(b, 0, 0.25, 190, 70, 4, 0.6, 12); Noise(b, 0, 0.08, 0.38, 1, 35, 41); Lowpass(b, 0.45); Tone(b, 0, 0.06, 1600, 900, 2, 0.08, 30); break;
                case "brawl-smash": b = Buf(0.6); Tone(b, 0, 0.5, 150, 40, 4, 0.75, 6); Noise(b, 0, 0.18, 0.5, 1, 18, 42); Lowpass(b, 0.4); Tone(b, 0, 0.1, 2200, 1200, 2, 0.1, 25); break;
                case "brawl-reflect": b = Buf(0.3); Tone(b, 0, 0.25, 1500, 2600, 4, 0.18, 6); Tone(b, 0.03, 0.2, 2200, 3300, 4, 0.08, 8); break;
                case "brawl-fizzle": b = Buf(0.3); Noise(b, 0, 0.28, 0.25, 1, 8, 43); Lowpass(b, 0.6); break;
                case "brawl-boom": b = Buf(0.8); Noise(b, 0, 0.75, 0.55, 3, 5, 44); Lowpass(b, 0.2); Tone(b, 0, 0.5, 120, 40, 4, 0.5, 5); break;
                case "brawl-ko": b = Buf(1.1); Noise(b, 0, 0.6, 0.5, 2, 5, 45); Lowpass(b, 0.25); Tone(b, 0, 0.4, 140, 45, 4, 0.55, 5); Tone(b, 0.15, 0.9, 500, 2200, 4, 0.16, 2); break;
                case "brawl-twinkle": b = Buf(0.9); Tone(b, 0, 0.5, 600, 2200, 4, 0.14, 3); Notes(b, 0.45, 0.06, 2, 0.12, 96, 100, 103); break;
                case "brawl-bell": b = Buf(1.1); for (int i = 0; i < 3; i++) { Tone(b, i * 0.32, 0.6, 1320, 1320, 4, 0.16, 6); Tone(b, i * 0.32, 0.6, 3300, 3300, 4, 0.05, 9); } break;
                case "brawl-beep": b = Buf(1.0); for (int i = 0; i < 4; i++) Tone(b, i * 0.24, 0.12, 1250, 1250, 1, 0.09, 5); break;
                case "brawl-hum": b = Buf(1.3); Tone(b, 0, 1.25, 70, 120, 3, 0.14, 0.4); Tone(b, 0, 1.25, 140, 240, 4, 0.1, 0.4); Lowpass(b, 0.2); break;
                case "brawl-whistle": b = Buf(1.3); Tone(b, 0, 1.25, 2600, 700, 4, 0.14, 0.8); break;
                case "brawl-wind": b = Buf(1.4); Noise(b, 0, 1.35, 0.35, 1, 0.6, 46); Lowpass(b, 0.06); Tone(b, 0, 1.3, 300, 500, 4, 0.04, 0.5); break;
                // the javelina herd: a grown-up's nasal double snort, a baby's little squeak, and the patter of hooves
                case "brawl-snort": b = Buf(0.45); Tone(b, 0, 0.12, 150, 110, 0, 0.12, 10); Tone(b, 0.18, 0.14, 140, 95, 0, 0.12, 10); Noise(b, 0, 0.13, 0.6, 2, 14, 50); Noise(b, 0.18, 0.15, 0.55, 2, 12, 51); Lowpass(b, 0.18); break;
                case "brawl-squeal": b = Buf(0.25); Tone(b, 0, 0.18, 520, 760, 1, 0.12, 6); Tone(b, 0, 0.18, 1040, 1500, 4, 0.06, 6); Noise(b, 0, 0.15, 0.12, 1, 12, 52); Lowpass(b, 0.5); break;
                case "brawl-hooves": b = Buf(1.3); for (int i = 0; i < 16; i++) { double s = i * 0.07 + (i % 2) * 0.02; Tone(b, s, 0.05, 120, 70, 4, 0.35, 40); Noise(b, s, 0.03, 0.2, 2, 60, 53 + i); } Lowpass(b, 0.35); break;
                case "brawl-sudden": b = Buf(1.0); Notes(b, 0, 0.12, 0, 0.12, 69, 68, 67, 66, 65); break;
                case "brawl-game": b = Buf(0.7); Tone(b, 0, 0.6, 100, 50, 4, 0.5, 4); Noise(b, 0, 0.3, 0.3, 2, 8, 47); Lowpass(b, 0.3); break;
                case "brawl-go": b = Buf(0.5); Notes(b, 0, 0.09, 0, 0.12, 74, 78, 81, 86); break;
                default: b = Buf(0.05); break;
            }
            return Peak(b, 0.8);
        }
    }
}
