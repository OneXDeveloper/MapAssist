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
        [FieldOffset(0x858)] public IntPtr pAct;
        [FieldOffset(0x868)] public IntPtr pLevelFirst;
    }
}
