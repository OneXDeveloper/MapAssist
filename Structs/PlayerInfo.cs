using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PlayerInfo
    {
        [FieldOffset(0x0)] public IntPtr pPlayerInfo;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct PlayerInfoStrc
    {
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x5C)] public bool Expansion;
    }
}
