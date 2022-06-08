using System;
using System.Runtime.InteropServices;
using MapAssist.Types;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ActMisc
    {
        [FieldOffset(0x120)] public Area RealTombArea;
        [FieldOffset(0x830)] public Difficulty GameDifficulty;
        [FieldOffset(0x840)] public ulong dwInitSeedHash;
        [FieldOffset(0x860)] public IntPtr pAct;
        [FieldOffset(0x868)] public uint EndSeedHash;
        [FieldOffset(0x870)] public IntPtr pLevelFirst;
    }
}
