using MapAssist.Types;
using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Pet
    {
        [FieldOffset(0x00)] public readonly Npc TxtFileNo;
        [FieldOffset(0x08)] public readonly uint UnitId;
        [FieldOffset(0x0C)] public readonly uint OwnerId;
        [FieldOffset(0x30)] public readonly IntPtr pNext;
    }
}
