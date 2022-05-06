using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Path
    {
        [FieldOffset(0x00)] public ushort XOffset;
        [FieldOffset(0x02)] public ushort DynamicX;
        [FieldOffset(0x04)] public ushort YOffset;
        [FieldOffset(0x06)] public ushort DynamicY;
        [FieldOffset(0x10)] public ushort StaticX;
        [FieldOffset(0x14)] public ushort StaticY;
        [FieldOffset(0x20)] public IntPtr pRoom;
    }
}
