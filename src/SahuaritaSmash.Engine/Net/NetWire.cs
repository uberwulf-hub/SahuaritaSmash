// Network play, part 2: the messages, as small binary packets (the first byte says which). The same on every device;
// NetProtocol.Version must match or the host refuses ("update the game"), as Classic Games' LAN play did.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassicGames
{
    public static class NetProtocol
    {
        /// <summary>Changes whenever a message or the match's rules change in a way devices must agree on.</summary>
        public const int Version = 3;   // 2 (0.3.0): Gila Gus and the Pecan Orchard, so fighters and stages are counted differently; 3: Pecos Bill and Monsoon Wash, and Rematch (each match has a number, carried by its messages)
        /// <summary>Tells this game apart from others sharing the relay (Race Track Mayhem uses it too).</summary>
        public const string Game = "SahuaritaSmash";

        public const byte Hello = (byte)'H', Welcome = (byte)'W', Refuse = (byte)'R', Lobby = (byte)'L', Pick = (byte)'P',
            Start = (byte)'S', Inputs = (byte)'I', Check = (byte)'C', Drop = (byte)'D', End = (byte)'E', Cheer = (byte)'Q';

        /// <summary>A stick or key direction as the network carries it: -127..127, used on every device (the local
        /// player's too), so all of them run the match on exactly the same numbers.</summary>
        public static sbyte Axis(double v) { return (sbyte)Math.Max(-127, Math.Min(127, (int)Math.Round(v * 127))); }
        public static double Axis(sbyte v) { return v / 127.0; }

        public static int Pack(BrInput i)
        {
            int b = (i.Jump ? 1 : 0) | (i.Attack ? 2 : 0) | (i.Strong ? 4 : 0) | (i.Special ? 8 : 0) | (i.Shield ? 16 : 0) | (i.Grab ? 32 : 0);
            return b | ((byte)Axis(i.X) << 8) | ((byte)Axis(i.Y) << 16);
        }
        public static BrInput Unpack(int p)
        {
            return new BrInput
            {
                Jump = (p & 1) != 0, Attack = (p & 2) != 0, Strong = (p & 4) != 0, Special = (p & 8) != 0, Shield = (p & 16) != 0, Grab = (p & 32) != 0,
                X = Axis((sbyte)((p >> 8) & 255)), Y = Axis((sbyte)((p >> 16) & 255)),
            };
        }
    }

    /// <summary>Writes a packet.</summary>
    public sealed class NetWriter
    {
        readonly MemoryStream ms = new MemoryStream();
        readonly BinaryWriter w;
        public NetWriter(byte kind) { w = new BinaryWriter(ms, Encoding.UTF8); w.Write(kind); }
        public NetWriter B(byte v) { w.Write(v); return this; }
        public NetWriter I(int v) { w.Write(v); return this; }
        public NetWriter L(long v) { w.Write(v); return this; }
        public NetWriter U(uint v) { w.Write(v); return this; }
        public NetWriter S(string v) { w.Write(v ?? ""); return this; }
        public NetWriter Bool(bool v) { w.Write(v); return this; }
        public byte[] Done() { w.Flush(); return ms.ToArray(); }
    }

    /// <summary>Reads a packet; anything short or malformed throws, and the caller drops the packet.</summary>
    public sealed class NetReader
    {
        readonly BinaryReader r;
        public readonly byte Kind;
        public NetReader(byte[] data) { r = new BinaryReader(new MemoryStream(data), Encoding.UTF8); Kind = r.ReadByte(); }
        public byte B() { return r.ReadByte(); }
        public int I() { return r.ReadInt32(); }
        public long L() { return r.ReadInt64(); }
        public uint U() { return r.ReadUInt32(); }
        public string S() { string s = r.ReadString(); return s.Length > 64 ? s.Substring(0, 64) : s; }
        public bool Bool() { return r.ReadBoolean(); }
    }
}
