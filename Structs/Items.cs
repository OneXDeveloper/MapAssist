using System;
using System.Runtime.InteropServices;
using GameOverlay.Drawing;
using MapAssist.Types;

namespace MapAssist.Structs
{
    public struct Item
    {
        public Point Position;
        public uint UnitId;
        public ItemQuality Quality;
        public ItemFlags Flags;
        public string BaseItemName;
        public ItemMode Mode;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct ItemInventory
    {
        [FieldOffset(0x20)] public IntPtr InvGridPtr;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct ItemData
    {
        [FieldOffset(0x00)] public ItemQuality ItemQuality;
        //[FieldOffset(0x0C)] public StashType StashType; //only works for offline character
        [FieldOffset(0x0C)] public uint dwOwnerID; //which unitId owns this item (online only) - otherwise 0 = body, 1 = personal stash, 2 = sharedstash1, 3 = sharedstash2, 4 = sharedstash3, 5 = belt
        [FieldOffset(0x18)] public ItemFlags ItemFlags;
        [FieldOffset(0x34)] public uint uniqueOrSetId;
        [FieldOffset(0x42)] public short RarePrefix;
        [FieldOffset(0x44)] public short RareSuffix;
        [FieldOffset(0x46)] public short AutoAffix;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        [FieldOffset(0x48)] public ushort[] Affixes;
        [FieldOffset(0x54)] public BodyLoc BodyLoc;
        [FieldOffset(0x55)] public InvPage InvPage;
        [FieldOffset(0xA0)] public IntPtr pInventory;
        //[FieldOffset(0x88)] public byte nodePos; // char?
        //[FieldOffset(0x89)] public byte nodePosOther; // char?
    }
}
