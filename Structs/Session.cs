using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Session
    {
        [FieldOffset(0x30)] public byte GameNameLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        [FieldOffset(0x40)] public byte[] GameName;

        [FieldOffset(0x88)] public byte GamePassLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        [FieldOffset(0x98)] public byte[] GamePass;
    }
}
