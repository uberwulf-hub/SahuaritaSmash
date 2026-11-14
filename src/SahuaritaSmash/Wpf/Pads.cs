// Game controllers (App Hub's Classic Games, Engine\KartGame.cs: KartPads and KartPad, unchanged): Xbox-style pads
// through XInput, which is part of Windows. Elsewhere (Android) the DLL is missing and Read finds no pads; Fake lets
// tests (and later the touch controls) stand in for a pad.
using System;
using System.Runtime.InteropServices;

namespace ClassicGames
{
    /// <summary>Xbox-style game controllers through XInput (part of Windows). Read only; missing DLL = no pads.</summary>
    public static class KartPads
    {
        [StructLayout(LayoutKind.Sequential)]
        struct XState { public uint Packet; public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] static extern int GetState(int i, out XState s);
        static bool broken;
        /// <summary>Tests and snapshots turn the real controllers off (and may fake some).</summary>
        public static bool Enabled = true;
        public static readonly KartPad[] Fake = new KartPad[4];

        public static KartPad Read(int i)
        {
            if (Fake[i] != null) return Fake[i];
            var p = new KartPad();
            if (!Enabled || broken) return p;
            try
            {
                XState s;
                if (GetState(i, out s) != 0) return p;
                p.On = true;
                p.X = Math.Abs((int)s.LX) < 8000 ? 0 : Math.Max(-1, Math.Min(1, s.LX / 30000.0));
                p.Y = Math.Abs((int)s.LY) < 8000 ? 0 : Math.Max(-1, Math.Min(1, s.LY / 30000.0));
                int b = s.Buttons;
                p.Up = (b & 0x1) != 0; p.Down = (b & 0x2) != 0; p.Left = (b & 0x4) != 0; p.Right = (b & 0x8) != 0;
                p.Start = (b & 0x10) != 0; p.Back = (b & 0x20) != 0; p.LB = (b & 0x100) != 0; p.RB = (b & 0x200) != 0;
                p.A = (b & 0x1000) != 0; p.B = (b & 0x2000) != 0; p.XB = (b & 0x4000) != 0; p.YB = (b & 0x8000) != 0;
                p.LT = s.LT / 255.0; p.RT = s.RT / 255.0;
                p.RX = Math.Abs((int)s.RX) < 8000 ? 0 : Math.Max(-1, Math.Min(1, s.RX / 30000.0));
                p.RY = Math.Abs((int)s.RY) < 8000 ? 0 : Math.Max(-1, Math.Min(1, s.RY / 30000.0));
            }
            catch (Exception) { broken = true; }
            return p;
        }
    }

    public sealed class KartPad
    {
        public bool On, Up, Down, Left, Right, Start, Back, LB, RB, A, B, XB, YB;
        public double X, Y, LT, RT;
        /// <summary>The right stick (Sahuarita Smash's strong attacks).</summary>
        public double RX, RY;
        public double Steer { get { return Left ? -1 : (Right ? 1 : X); } }
        public bool Gas { get { return A || RT > 0.3; } }
        public bool Brake { get { return B || LT > 0.3; } }
        public bool Hop { get { return RB || XB; } }
        public bool Item { get { return LB || YB; } }
    }
}
