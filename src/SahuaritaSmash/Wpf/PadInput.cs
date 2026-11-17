// Game controllers that a platform reports as events (Android: Bluetooth or USB gamepads, Xbox / PlayStation /
// others), turned into KartPad values with the same meaning as XInput's on Windows: X / Y the left stick (up is +),
// RX / RY the right stick (strong attacks), LT / RT the triggers 0..1, A B XB YB, LB RB, Start Back, the d-pad.
// Everything here is plain C# so the checks can drive it with pretend events. The numbers are Android's own key codes
// and axis numbers (KeyEvent.KEYCODE_*, MotionEvent.AXIS_*), which never change; the activity only passes them on.
// Slots: a controller gets the lowest free slot 0-3 the first time someone uses it (a button, or a stick or trigger
// pushed well past its middle), so "controller 1" is the first one picked up, as on Windows; a controller that goes
// away frees its slot. Windows never feeds this, so XInput there is unchanged.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public sealed class KartPadInput
    {
        // Android's key codes (KeyEvent.KEYCODE_*)
        public const int KeyDpadUp = 19, KeyDpadDown = 20, KeyDpadLeft = 21, KeyDpadRight = 22, KeyDpadCenter = 23;
        public const int KeyA = 96, KeyB = 97, KeyC = 98, KeyX = 99, KeyY = 100, KeyZ = 101, KeyL1 = 102, KeyR1 = 103,
            KeyL2 = 104, KeyR2 = 105, KeyThumbL = 106, KeyThumbR = 107, KeyStart = 108, KeySelect = 109;
        // Android's axis numbers (MotionEvent.AXIS_*)
        public const int AxisX = 0, AxisY = 1, AxisZ = 11, AxisRz = 14, AxisHatX = 15, AxisHatY = 16, AxisLTrigger = 17,
            AxisRTrigger = 18, AxisGas = 22, AxisBrake = 23;
        /// <summary>The axes the activity reads from every joystick event.</summary>
        public static readonly int[] Axes = { AxisX, AxisY, AxisZ, AxisRz, AxisHatX, AxisHatY, AxisLTrigger, AxisRTrigger, AxisGas, AxisBrake };

        /// <summary>The middle of a stick that counts as resting (XInput's 8000 of 32768 on Windows).</summary>
        public const double DeadZone = 0.25;
        /// <summary>How far past the middle a stick or trigger must go to claim a slot (resting sticks drift).</summary>
        public const double WakeUp = 0.5;

        sealed class Dev
        {
            public int Slot = -1;
            public readonly HashSet<int> Keys = new HashSet<int>();
            public readonly double[] Axis = new double[32];
        }

        readonly object gate = new object();
        readonly Dictionary<int, Dev> devs = new Dictionary<int, Dev>();
        readonly int[] slotDev = { -1, -1, -1, -1 };
        readonly Queue<string> news = new Queue<string>();
        long lastLook;

        /// <summary>Asked about once a second for every known controller: is it still there? (Android:
        /// InputDevice.GetDevice(id) != null.) Null = never asked; the platform calls Gone instead.</summary>
        public Func<int, bool> Exists;
        /// <summary>The clock for Exists (milliseconds; the checks set their own).</summary>
        public Func<long> Now = () => Environment.TickCount64;

        /// <summary>True for the key codes a controller's buttons send (the activity takes these, and only from a
        /// gamepad or joystick, so the phone's own keys are left alone).</summary>
        public static bool IsPadKey(int code)
        {
            return code >= KeyDpadUp && code <= KeyDpadCenter || code >= KeyA && code <= KeySelect;
        }

        /// <summary>A controller button went down or up.</summary>
        public void Key(int device, int code, bool down)
        {
            if (!IsPadKey(code)) return;
            lock (gate)
            {
                Dev d = Get(device);
                if (down) { d.Keys.Add(code); Claim(device, d); } else d.Keys.Remove(code);
            }
        }

        /// <summary>A controller's axis moved (Android's raw value: sticks -1..1 with down as +, triggers 0..1).</summary>
        public void Axis(int device, int axis, double value)
        {
            if (axis < 0 || axis >= 32 || double.IsNaN(value)) return;
            lock (gate)
            {
                Dev d = Get(device);
                d.Axis[axis] = value;
                if (Math.Abs(value) >= WakeUp) Claim(device, d);
            }
        }

        /// <summary>A controller was switched off or disconnected: its slot is free again.</summary>
        public void Gone(int device)
        {
            lock (gate)
            {
                Dev d;
                if (!devs.TryGetValue(device, out d)) return;
                devs.Remove(device);
                if (d.Slot >= 0) { slotDev[d.Slot] = -1; news.Enqueue("Controller " + (d.Slot + 1) + " disconnected"); }
            }
        }

        /// <summary>Forgets every controller (the checks).</summary>
        public void Reset()
        {
            lock (gate) { devs.Clear(); for (int i = 0; i < 4; i++) slotDev[i] = -1; news.Clear(); }
        }

        /// <summary>The slot a controller has, or -1.</summary>
        public int SlotOf(int device)
        {
            lock (gate) { Dev d; return devs.TryGetValue(device, out d) ? d.Slot : -1; }
        }

        /// <summary>"Controller 1 connected" and the like, once each, for the game to show.</summary>
        public string TakeNews()
        {
            lock (gate) return news.Count > 0 ? news.Dequeue() : null;
        }

        /// <summary>The controller in a slot now, or null when no controller has it.</summary>
        public KartPad Read(int slot)
        {
            if (slot < 0 || slot > 3) return null;
            LookForGone();
            lock (gate)
            {
                int id = slotDev[slot];
                Dev d;
                if (id < 0 || !devs.TryGetValue(id, out d)) return null;
                var p = new KartPad { On = true };
                p.X = Stick(d.Axis[AxisX]);
                p.Y = -Stick(d.Axis[AxisY]);          // Android's down is +, XInput's up is +
                p.RX = Stick(d.Axis[AxisZ]);
                p.RY = -Stick(d.Axis[AxisRz]);
                p.LT = Math.Max(Trigger(d.Axis[AxisLTrigger]), Trigger(d.Axis[AxisBrake]));
                p.RT = Math.Max(Trigger(d.Axis[AxisRTrigger]), Trigger(d.Axis[AxisGas]));
                if (d.Keys.Contains(KeyL2)) p.LT = 1;
                if (d.Keys.Contains(KeyR2)) p.RT = 1;
                p.Up = d.Keys.Contains(KeyDpadUp) || d.Axis[AxisHatY] < -0.5;
                p.Down = d.Keys.Contains(KeyDpadDown) || d.Axis[AxisHatY] > 0.5;
                p.Left = d.Keys.Contains(KeyDpadLeft) || d.Axis[AxisHatX] < -0.5;
                p.Right = d.Keys.Contains(KeyDpadRight) || d.Axis[AxisHatX] > 0.5;
                p.A = d.Keys.Contains(KeyA); p.B = d.Keys.Contains(KeyB); p.XB = d.Keys.Contains(KeyX); p.YB = d.Keys.Contains(KeyY);
                p.LB = d.Keys.Contains(KeyL1); p.RB = d.Keys.Contains(KeyR1);
                p.Start = d.Keys.Contains(KeyStart); p.Back = d.Keys.Contains(KeySelect);
                return p;
            }
        }

        /// <summary>A stick as XInput's are read on Windows: the middle quarter is rest, the rest scaled to reach 1.</summary>
        public static double Stick(double v)
        {
            if (Math.Abs(v) < DeadZone) return 0;
            return Math.Max(-1, Math.Min(1, v / 0.915));
        }

        static double Trigger(double v) { return Math.Max(0, Math.Min(1, v)); }

        Dev Get(int device)
        {
            Dev d;
            if (!devs.TryGetValue(device, out d)) { d = new Dev(); devs[device] = d; }
            return d;
        }

        void Claim(int device, Dev d)
        {
            if (d.Slot >= 0) return;
            for (int s = 0; s < 4; s++)
                if (slotDev[s] < 0) { slotDev[s] = device; d.Slot = s; news.Enqueue("Controller " + (s + 1) + " connected"); return; }
        }

        void LookForGone()
        {
            Func<int, bool> exists = Exists;
            if (exists == null) return;
            long now = Now();
            if (now - lastLook < 1000 && lastLook != 0) return;
            lastLook = now;
            List<int> ids;
            lock (gate) ids = new List<int>(devs.Keys);
            foreach (int id in ids)
            {
                bool there;
                try { there = exists(id); } catch (Exception) { there = true; }
                if (!there) Gone(id);
            }
        }

        /// <summary>Touch and a controller together as one person's controller 1: either one's buttons count, and the
        /// stick pushed further wins.</summary>
        public static KartPad Merge(KartPad a, KartPad b)
        {
            var p = new KartPad
            {
                On = a.On || b.On,
                Up = a.Up || b.Up, Down = a.Down || b.Down, Left = a.Left || b.Left, Right = a.Right || b.Right,
                Start = a.Start || b.Start, Back = a.Back || b.Back, LB = a.LB || b.LB, RB = a.RB || b.RB,
                A = a.A || b.A, B = a.B || b.B, XB = a.XB || b.XB, YB = a.YB || b.YB,
                LT = Math.Max(a.LT, b.LT), RT = Math.Max(a.RT, b.RT),
            };
            p.X = Math.Abs(a.X) >= Math.Abs(b.X) ? a.X : b.X;
            p.Y = Math.Abs(a.Y) >= Math.Abs(b.Y) ? a.Y : b.Y;
            p.RX = Math.Abs(a.RX) >= Math.Abs(b.RX) ? a.RX : b.RX;
            p.RY = Math.Abs(a.RY) >= Math.Abs(b.RY) ? a.RY : b.RY;
            return p;
        }
    }
}
