using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    /// <summary>
    /// Partly implemented - Source: D2ObjectDataStrc https://github.com/ThePhrozenKeep/D2MOO/blob/master/source/D2Common/include/Units/Object.h#L32
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ObjectData
    {
        [FieldOffset(0x00)] public ObjectTxt pObjectTxt;
//        [FieldOffset(0x04)] public sbyte InteractType;
//        [FieldOffset(0x05)] public sbyte nPortalFlags;
//        [FieldOffset(0x06)] public short unk0x06;
        [FieldOffset(0x08)] public Shrine Shrine;
//        [FieldOffset(0x0C)] public uint dwOperateGUID;
//        [FieldOffset(0x10)] public bool bPermanent;
//        [FieldOffset(0x14)] public uint __014;
    }

    /// <summary>
    /// Partly implemented - Source: D2ShrinesTxt https://github.com/ThePhrozenKeep/D2MOO/blob/master/source/D2Common/include/DataTbls/ObjectsTbls.h#L122
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Shrine
    {
        [FieldOffset(0x00)] public ShrineType ShrineType;
//        [FieldOffset(0x01)] public sbyte pad0x01;                     //0x01
//        [FieldOffset(0x04)] public uint dwArg0;                        //0x04
//        [FieldOffset(0x08)] public uint dwArg1;                        //0x08
//        [FieldOffset(0x0C)] public uint dwDurationInFrames;            //0x0C
//        [FieldOffset(0x10)] public sbyte nResetTimeInMins;               //0x10
//        [FieldOffset(0x11)] public sbyte nRarity;                        //0x11
//        [FieldOffset(0x12)] public char szViewname;                    //0x12
//        [FieldOffset(0x32)] public char szNiftyphrase;                //0x32
//        [FieldOffset(0xB2)] public sbyte nEffectClass;                   //0xB2
//        [FieldOffset(0xB3)] public sbyte pad0xB3;                        //0xB3
//        [FieldOffset(0xB4)] public uint dwLevelMin;                    //0xB4
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct ObjectTxt
    {
        [FieldOffset(0x00)] public IntPtr Name;						//0x00
//        [FieldOffset(0x167)] public byte nSubClass;						//0x167
//        [FieldOffset(0x16F)] public byte nShrineFunction;				//0x16F
    }

    public enum ObjectSubClass
    {
        OBJSUBCLASS_SHRINE = 0x01,
        OBJSUBCLASS_OBELISK = 0x02,
        OBJSUBCLASS_TOWNPORTAL = 0x04,
        OBJSUBCLASS_CHEST = 0x08,
        OBJSUBCLASS_PORTAL = 0x10,
        OBJSUBCLASS_WELL = 0x20,
        OBJSUBCLASS_WAYPOINT = 0x40,
        OBJSUBCLASS_DOOR = 0x80,
    };

    public enum ShrineType
    {
        SHRINE_NONE,
        SHRINE_REFILL,
        SHRINE_HPBOOST,
        SHRINE_MANABOOST,
        SHRINE_HPXCHANGE,
        SHRINE_MANAXCHANGE,
        SHRINE_ARMOR,
        SHRINE_COMBAT,
        SHRINE_RESFIRE,
        SHRINE_RESCOLD,
        SHRINE_RESLITE,
        SHRINE_RESPOIS,
        SHRINE_SKILL,
        SHRINE_MANAREGEN,
        SHRINE_STAM,
        SHRINE_EXP,
        SHRINE_ENIRHS,
        SHRINE_PORTAL,
        SHRINE_GEM,
        SHRINE_STORM,
        SHRINE_WARP,
        SHRINE_EXPLOSION,
        SHRINE_POISON
    }
}
