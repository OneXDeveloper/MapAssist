using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct MonsterData
    {
        [FieldOffset(0x0)] public IntPtr pMonStats;
        [FieldOffset(0x8)] public ulong ShrineType;
        [FieldOffset(0x1A)] public MonsterTypeFlags MonsterType;
        [FieldOffset(0x2A)] public ushort BossLineID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MonStats
    {
        private ushort Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Name;
    }

    [Flags]
    public enum MonsterTypeFlags : byte
    {
        None = 0,
        Other = 1,
        SuperUnique = 1 << 1,
        Champion = 1 << 2,
        Unique = 1 << 3,
        Minion = 1 << 4,
        Possessed = 1 << 5,
        Ghostly = 1 << 6,
        Multishot = 1 << 7,
    }
}
