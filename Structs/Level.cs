using System.Runtime.InteropServices;
using MapAssist.Types;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Level
    {
        [FieldOffset(0x1F8)] public Area LevelId;
    }
}
