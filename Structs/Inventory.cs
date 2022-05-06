using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Inventory
    {
        [FieldOffset(0x0)] public uint dwSignature;
        [FieldOffset(0x8)] public IntPtr pOwnerUnit;
        [FieldOffset(0x20)] public IntPtr pGrid;

        // Used to see if the current player is the correct player
        // Should not be 0x0 for local player
        [FieldOffset(0x70)] public IntPtr pUnk1;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct InventoryGrid
    {
        [FieldOffset(0x50)] public byte Width;
        [FieldOffset(0x51)] public byte Height;
        [FieldOffset(0xD0)] public byte StashWidth;
        [FieldOffset(0xD1)] public byte StashHeight;
    }
}
