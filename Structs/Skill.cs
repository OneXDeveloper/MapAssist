using MapAssist.Types;
using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SkillList
    {
        [FieldOffset(0x0)] public IntPtr pFirstSkill; //pointer to a Skill
        [FieldOffset(0x08)] public IntPtr pLeftSkill;
        [FieldOffset(0x10)] public IntPtr pRightSkill;
        [FieldOffset(0x18)] public IntPtr pUsedSkill;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SkillStrc
    {
        [FieldOffset(0x0)] public IntPtr SkillTxt;
        [FieldOffset(0x08)] public IntPtr pNextSkill;
        [FieldOffset(0x34)] public uint HardPoints;
        [FieldOffset(0x3C)] public uint Quantity;
        [FieldOffset(0x44)] public uint Charges;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SkillTxt
    {
        [FieldOffset(0x0)] public Skill Id;
    }
}
