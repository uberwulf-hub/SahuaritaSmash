// Classic Games - Mesa Mayhem's keys: the two sets on one keyboard (left and right), which a person can change on the
// Controls screen (Andrew, 2026-10-29: "allow a person to remap the keys from the main menu"). Each set has one key per
// action; giving a key that is already used to another action swaps the two. The extra keys (Space jumps for the left
// set, the number pad for the right, Z X C V for a person alone) keep working unless a set now uses that key.
// Saved per player in brawl-keys.txt next to the scores: "name<TAB>1<TAB>A,D,W,...<TAB>Left,Right,Up,..." (Key names;
// fields after these are kept as they are; a name that isn't a key, or a key that isn't allowed, falls back to the usual).
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed class BrawlKeys
    {
        public const int Count = 9;
        public const int MoveLeft = 0, MoveRight = 1, Jump = 2, Down = 3, Attack = 4, Strong = 5, Special = 6, Shield = 7, Grab = 8;
        public static readonly string[] Actions = { "Move left", "Move right", "Jump / up", "Down / drop", "Attack", "Strong", "Special", "Shield", "Grab" };
        public static readonly Key[] LeftUsual = { Key.A, Key.D, Key.W, Key.S, Key.F, Key.G, Key.H, Key.R, Key.T };
        public static readonly Key[] RightUsual = { Key.Left, Key.Right, Key.Up, Key.Down, Key.K, Key.L, Key.OemSemicolon, Key.O, Key.I };
        public static readonly string[] SetNames = { "Left keys", "Right keys" };

        public readonly Key[][] Set = { (Key[])LeftUsual.Clone(), (Key[])RightUsual.Clone() };

        public bool IsUsual { get { return Set[0].SequenceEqual(LeftUsual) && Set[1].SequenceEqual(RightUsual); } }
        public void Reset() { Set[0] = (Key[])LeftUsual.Clone(); Set[1] = (Key[])RightUsual.Clone(); }

        /// <summary>Keys the game or Classic Games already uses (pause, music, sounds, menus, the timing panel), and
        /// keys that aren't real keys.</summary>
        public static bool Allowed(Key k)
        {
            if (k == Key.None || k == Key.Escape || k == Key.P || k == Key.M || k == Key.N || k == Key.Enter || k == Key.Tab || k == Key.Back) return false;
            if (k == Key.LWin || k == Key.RWin || k == Key.Apps || k == Key.System || k == Key.ImeProcessed || k == Key.DeadCharProcessed) return false;
            if (k == Key.CapsLock || k == Key.NumLock || k == Key.Scroll || k == Key.PrintScreen || k == Key.Pause) return false;
            if (k >= Key.F1 && k <= Key.F24) return false;
            return true;
        }

        /// <summary>Gives set s's action a the key k; if another action (in either set) had it, that one gets a's old key.</summary>
        public void Assign(int s, int a, Key k)
        {
            if (!Allowed(k)) return;
            Key old = Set[s][a];
            for (int t = 0; t < 2; t++)
                for (int b = 0; b < Count; b++)
                    if ((t != s || b != a) && Set[t][b] == k) Set[t][b] = old;
            Set[s][a] = k;
        }

        public bool Uses(Key k) { return Set[0].Contains(k) || Set[1].Contains(k); }
        /// <summary>An extra key (Space, the number pad, Z X C V) works while no set uses it for something else.</summary>
        public bool Extra(Key k) { return !Uses(k); }

        // ---------------------------------------------------------------- saving
        public string Fields() { return string.Join(",", Set[0].Select(k => k.ToString()).ToArray()) + "\t" + string.Join(",", Set[1].Select(k => k.ToString()).ToArray()); }

        /// <summary>Reads the two sets from a saved line's fields (after the name and version); anything odd keeps the usual key.</summary>
        public void Read(string left, string right)
        {
            Reset();
            ReadSet(0, left); ReadSet(1, right);
            // two actions with one key (an edited file): the later ones go back to the usual key, if that is free
            var seen = new HashSet<Key>();
            for (int s = 0; s < 2; s++)
                for (int a = 0; a < Count; a++)
                {
                    Key k = Set[s][a];
                    if (seen.Contains(k)) { Key u = s == 0 ? LeftUsual[a] : RightUsual[a]; if (!seen.Contains(u) && !Set[s].Skip(a + 1).Contains(u) && (s == 1 || !Set[1].Contains(u))) k = u; Set[s][a] = k; }
                    seen.Add(Set[s][a]);
                }
        }
        void ReadSet(int s, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] p = text.Split(',');
            for (int a = 0; a < Count && a < p.Length; a++)
            {
                Key k;
                if (Enum.TryParse(p[a].Trim(), false, out k) && Enum.IsDefined(typeof(Key), k) && Allowed(k)) Set[s][a] = k;
            }
        }

        // ---------------------------------------------------------------- names a person reads
        public static string Name(Key k)
        {
            switch (k)
            {
                case Key.Left: return ((char)0x2190).ToString();
                case Key.Up: return ((char)0x2191).ToString();
                case Key.Right: return ((char)0x2192).ToString();
                case Key.Down: return ((char)0x2193).ToString();
                case Key.Space: return "Space";
                case Key.OemSemicolon: return ";";
                case Key.OemQuotes: return "'";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemPipe: return ((char)92).ToString();
                case Key.OemMinus: return "-";
                case Key.OemPlus: return "=";
                case Key.OemTilde: return "`";
                case Key.LeftShift: return "Left Shift";
                case Key.RightShift: return "Right Shift";
                case Key.LeftCtrl: return "Left Ctrl";
                case Key.RightCtrl: return "Right Ctrl";
                case Key.LeftAlt: return "Left Alt";
                case Key.RightAlt: return "Right Alt";
                case Key.PageUp: return "Page Up";
                case Key.PageDown: return "Page Down";
                case Key.Insert: return "Insert";
                case Key.Delete: return "Delete";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.Add: return "num +";
                case Key.Subtract: return "num -";
                case Key.Multiply: return "num *";
                case Key.Divide: return "num /";
                case Key.Decimal: return "num .";
            }
            if (k >= Key.D0 && k <= Key.D9) return ((char)('0' + (k - Key.D0))).ToString();
            if (k >= Key.NumPad0 && k <= Key.NumPad9) return "num " + (char)('0' + (k - Key.NumPad0));
            return k.ToString();
        }
    }
}
