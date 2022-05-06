using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct MapSeed
    {
        [FieldOffset(0x110)] public uint check; // User feedback that this check worked 100% of the time from the people that tried it out
        //[FieldOffset(0x124)] public uint check; // User feedback that this check worked 100% of the time from the people that tried it out
        //[FieldOffset(0x830)] public uint check; // User feedback that this check worked most of the time from the people that tried it out

        [FieldOffset(0x840)] public uint mapSeed1;
        [FieldOffset(0x10C0)] public uint mapSeed2;
    }
}
