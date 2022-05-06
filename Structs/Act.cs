using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Act
    {
        [FieldOffset(0x1C)] public uint MapSeed;
        [FieldOffset(0x28)] public uint ActId;
        [FieldOffset(0x78)] public IntPtr pActMisc;
    }
}
