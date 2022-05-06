using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct RoomEx
    {
        [FieldOffset(0x90)] public IntPtr pLevel;
    }
}
